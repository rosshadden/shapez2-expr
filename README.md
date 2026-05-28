# Shapez 2 - Expr

Expression gate for Shapez 2.

## Description

This is directly inspired by the E2 gate in GMod/Wiremod.

Allows Encoding more kinds of values into the existing wire system, such as arbitrary strings.
These are not viewable by normal means (hovering, monitor, etc.) but are indeed transmitted.

Also adds a monitor clone that prints out the raw value of wire signals.
So for example if you give it a `CuCuCuCu` signal you will see that string, not the shape which you would see with the vanilla monitor.

### Expressions

The expression syntax is [NCalc](https://ncalc.github.io/ncalc/articles/).

I added more vars and functions to it to make it work great with the Shapez 2 ecosystem:

```excel
# variables
a b c tick

# functions
rotate(s) paint(s, color) stack(bot, top) layer(s, n) quadrant(s, n)
isNull(v) isShape(v) isColor(v)
len(s) substr(s, i, n) concat(a, b, ...)
get(k) set(k, v)
```

I'll add docs for these eventually, but they are mostly intuitive.
`tick` is an auto-incrementing variable that increments every tick.

`get` and `set` might be a little overpowered.
They allow you to store values in a key-value store, which is useful for latches, counters, and other memory gates.
But it is (right now at least) a global store, which means accessible from all expression gates.
In a game where we literally have an infinite range signal transmitter/receiver that's probably not a huge deal, but those are at least 3 layers high and 4x4 not counting the signal definition inputs.
I might make them use those or something instead, idk.
For now I just wanted to get all the low hanging fruit ideas I had floating around implemented.

## Roadmap

- [ ] real sprites (I'm currently just repurposing vanilla ones)
- [ ] proper docs
- [ ] images in said docs
- [ ] bigger monitors, or maybe a writable variant of the built-in label
- [ ] probably more functions in the expression runtime

## Usage

```
dotnet new install Raphdf201.Shapez2Template
dotnet new shapez2mod -n MyMod -o ./MyMod
```

dont forget to customize the manifest.json

to uninstall the template : `dotnet new uninstall Raphdf201.Shapez2Template`

## Releasing new versions

```
nuget pack .\.nuspec -NoDefaultExcludes
```
