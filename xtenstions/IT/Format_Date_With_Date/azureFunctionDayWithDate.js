module.exports = function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');
        var d = req.body.date;
        var l = req.body.language;
        var options = {weekday: 'long', year: 'numeric', month: 'long', day: 'numeric'};

        res = d.toLocaleDateString(l, options);
    context.done(null, res);
};