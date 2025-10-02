Basis Avatar Optimizer
====

**Basis Avatar Optimizer** is a work in progress avatar optimization processor that only works on Basis Framework avatars
(for the time being). 

It was not designed for avatars that use the Animator system.

The following has been implemented so far:

- GameObject optimization:
  - Disabled GameObjects are removed.
- Blendshape optimization:
  - Keeps track of which blendshapes vary (face tracking, viseme, blink) and keeps them.
  - Bakes non-varying blendshapes into the mesh (only if all SMRs that reference that mesh have the same value).
  - Removes unused blendshapes.
