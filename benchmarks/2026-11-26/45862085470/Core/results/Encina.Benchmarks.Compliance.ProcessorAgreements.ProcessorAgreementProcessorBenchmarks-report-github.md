```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error     | StdDev   | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|----------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 1,534.8 ns | 265.74 ns | 14.57 ns |  1.00 |    2 | 0.0324 | 0.0305 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         |   844.2 ns | 150.76 ns |  8.26 ns |  0.55 |    1 | 0.0248 | 0.0238 |     440 B |        0.77 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   783.4 ns |  55.85 ns |  3.06 ns |  0.51 |    1 | 0.0248 | 0.0238 |     440 B |        0.77 |
