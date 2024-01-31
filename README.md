# Accessibility (A11Y) Public Sample: LetterSpell

This sample Unity project implements the game LetterSpell, which is compatible with screen readers on mobile platforms. It demonstrates how to convert UI made with Unity, using uGUI (i.e. with a Canvas component), into data compatible with screen readers. Additionally, this sample shows how to read some system accessibility settings and apply them to the game.

Thanks for checking out this sample!

## System Requirements

You can run this sample project in the Unity Editor:
* Requires Unity 2023.3.0a17 and above.

You can build this sample project for the following platforms:
* Android 8 and above.
* iOS 13 and above.

## Controls

In the Unity Editor:
* Click and drag the letter card to the desired position.

On mobile devices, without the screen reader:
* Tap and drag the letter card to the desired position.

On mobile devices, with the screen reader:
* Tap a letter card or use swipe gestures to focus on the desired letter card.
* Double tap to select the focused letter card.
* Use swipe gestures to move the letter card forward or backward OR tap another card to move the focused card to that position.

## Features Not Implemented

Some UI elements in the Options screen are there just to demonstrate how they can be converted into data compatible with screen readers and are not actually functional. These are:
* The option search field
* The color theme dropdown
* The display size slider

## Known Issues

* On iOS, when in landscape, the accessibility focus indicator may be displayed with a smaller width than the actual width. This seems to appear for elements with a long width. This is an iOS bug, which we have already reported to Apple.
* The letter cards' text style doesn't always update according to the system accessibility settings.

## Additional Resources

Check out the documentation of the [Accessibility module APIs](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/UnityEngine.AccessibilityModule.html).

## Questions or Issues?

Come talk to us on the [Unity Accessibility Forum](https://forum.unity.com/forums/accessibility.922/)!

# Acknowledgement

LetterSpell is inspired by the iOS game [Letter Rooms](https://apps.apple.com/us/app/letter-rooms/id1563407977).

# License

Our intention is that you can use everything in this project as a starting point or as bits and pieces in your own Unity games. For the legal words, see [LICENSE.md](LICENSE.md).
