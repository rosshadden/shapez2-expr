using System;
using Expr.Scripting;
using Game.Content.Features.Signals;
using Game.Content.Features.Signals.Conductor;
using Game.Content.Features.Signals.Connections;
using Game.Content.Features.Signals.Simulation;
using Game.Content.Features.Signals.Tick;
using Game.Core.Simulation;
using ILogger = Core.Logging.ILogger;

namespace Expr;

// 1×1 gate today: one input pin labeled "a", one output pin labeled "b".
// Future variants will declare more pins; labels must stay unique across
// inputs and outputs because Shapez 2 wire connectors are direction-fixed
// at build time (see shapez2-wire-connector-model memory).
public class GateSimulation : Simulation<GateSimulationState>, ISignalSimulation, IUpdatableSimulation {
	public static ILogger Log;

	private const string InLabel  = "a";
	private const string OutLabel = "b";

	private readonly SignalConductorInput _in;
	private readonly SignalConductorOutput _out = new();
	private readonly SignalCodec _codec;
	private IScriptedGate _gate;
	private string _loadedScript;
	private bool _loggedFirstTick;

	public GateSimulation(GateSimulationState state, SignalCodec codec) : base(state) {
		_in = new SignalConductorInput(state.In0);
		_codec = codec;
	}

	public int NumSignalProviders => 1;
	public int NumSignalReceivers => 1;
	public ISignalProvider GetSignalProvider(int i) => _out;
	public ISignalReceiver GetSignalReceiver(int i) => _in;

	public void Update(Ticks startTicks, Ticks deltaTicks) {
		int n = SignalSimulation.GetAmountOfSignalsThisUpdate(startTicks, deltaTicks);
		if (n <= 0) return;

		EnsureScriptLoaded();

		var baseTick = SignalTicks.FromTicks(startTicks);
		for (int s = 0; s < n; s++) {
			var tick = baseTick + new SignalTicks(s);
			_in.TryPopSignal(startTicks, tick, out var sig);

			ISignal output;
			if (_gate != null) {
				var inEncoded = _codec.Encode(sig);
				_gate.SetInput(InLabel, inEncoded);
				var ok = _gate.Tick();
				var outEncoded = ok ? _gate.GetOutput(OutLabel) : null;
				output = ok ? _codec.Decode(outEncoded) : NullSignal.Instance;
				if (!_loggedFirstTick) {
					_loggedFirstTick = true;
					Log?.Info?.Log($"[Expr] gate: first tick ran. in(a)='{inEncoded}', tickOk={ok}, out(b)='{outEncoded}', pushedSignal={output}");
				}
			} else {
				// No script (or empty/whitespace, or failed to load): passthrough
				// so the gate isn't inert when the user first places it AND we
				// don't pay any Eagle interpreter cost.
				output = sig ?? NullSignal.Instance;
			}
			_out.PushSignal(output, startTicks, tick);
		}
	}

	private void EnsureScriptLoaded() {
		var script = State.Script ?? "";
		if (script == _loadedScript) return;
		_loadedScript = script;
		_loggedFirstTick = false;

		// Empty/whitespace script: tear down any interpreter so the per-tick
		// fast path is a pure passthrough. Eagle eval is far too expensive to
		// run on every signal slot (60+ times/sec) for a no-op script.
		if (string.IsNullOrWhiteSpace(script)) {
			_gate?.Dispose();
			_gate = null;
			Log?.Info?.Log("[Expr] gate: script cleared, passthrough mode");
			return;
		}

		try {
			if (_gate == null) {
				if (ScriptedGateFactory.Create == null) {
					Log?.Error?.Log("[Expr] gate: ScriptedGateFactory.Create is null! Bridge.Initialize never ran?");
					return;
				}
				_gate = ScriptedGateFactory.Create.Invoke(Log, script);
				Log?.Info?.Log($"[Expr] gate: script loaded ({script.Length} chars), _gate={(_gate != null ? "ok" : "NULL!")}");
			} else {
				_gate.LoadScript(script);
				Log?.Info?.Log($"[Expr] gate: script reloaded ({script.Length} chars)");
			}
		} catch (Exception ex) {
			Log?.Error?.Log("[Expr] gate: script load threw");
			Log?.Exception?.LogException(ex);
			_gate?.Dispose();
			_gate = null;
		}
	}
}
