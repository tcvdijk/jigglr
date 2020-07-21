# Introduction video

[![Youtube Link](https://img.youtube.com/vi/M6VcD3chx8k/0.jpg)](https://www.youtube.com/watch?v=M6VcD3chx8k)

# The Polygon Jigglr

Hi, I'm Thomas and this is the Polygon Jiggler. Let this be a warning: if you give your prototype a silly name, it may stick - so here we are. The system uses *local search* to improve how well a polygon fits to an underlying image. That's kind of like active contours or snakes, but here we use a straight-line polygon, because we're interested in building footprints and the corners are important to us.

In the video I demonstrate how this works: the polygon is attracted to *dark pixels*. About as fast as it can, the program randomly move the vertices of the polygon a small amount. If that makes the average brightness under the edges less, then it keeps the change. Otherwise it undoes and try something else. We "jiggle" the polygon, if you will.

And *that's* all. There's nothing here that you would reasonably call "machine learning" or anything. It's just a *simple* objective function, and *simple* hill climbing procedure that goes straight to the *local* optimum. This is precisely what we want for this system - not something like simulated annealing. We explicitly *do not* want to escape local optima. For *global* optimisation you want that, be *we* want a human to get *close enough* so that the search is attracted to the correct thing.
