Use `monitor.ps1 <script.fsx>` to produce result to `output/script.html` and
keep monitoring for file change.  If there's a file change, you'll need to
manually refresh the browser.

`monitor.ps1` internally uses `fslab.fsx` which provides finer control.
`fslab.fsx` does implement a file change monitoring mechanism but is not used
due to fslab library [memory leak
issue](https://github.com/fslaborg/FsLab/issues/121).
