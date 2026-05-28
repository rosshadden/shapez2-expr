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
rotate(shape) paint(shape, color) stack(bot, top) layer(shape, n) quadrant(shape, n)
isNull(value) isShape(value) isColor(value)
len(string) substr(string, start, count) concat(string1, string2, ...)
get(key) set(key, value)
```

I'll add docs for these eventually, but they are mostly intuitive.
`tick` is an auto-incrementing variable that increments every tick.

## Roadmap

- [ ] real sprites (I'm currently just repurposing vanilla and sample ones)
- [ ] add variables to the gate sprite, so the inputs are clear (either just statically in the image or rendered overlays or shaders or something)
- [ ] vet adding a dynamic overlay to the gate itself that shows its code (at _least_ show it on hover like vanilla components do)
- [ ] proper docs
- [ ] images in said docs
- [ ] bigger monitors, or maybe a writable variant of the built-in label
- [ ] probably more functions in the expression runtime

## Musings

My first pass on this actually embedded an entire programming language runtime and let you work with it in the gate.
It embedded TCL using the Eagle runtime.
I thought this would be a great idea, but it turns out it was kind of lame.
Some combination of having too much power and not enough to do with it.

I could see an embeddable lisp language fitting pretty well (Janet I'm looking at you) after seeing how much composing I'm doing with the functions added so far:
```
paint(stack(b, rotate(c)), a))
```
But I'm really happy with the simplicity of the current expression language implementation for now.

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
