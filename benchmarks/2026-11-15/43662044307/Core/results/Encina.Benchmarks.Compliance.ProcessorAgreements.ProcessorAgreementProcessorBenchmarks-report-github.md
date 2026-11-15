```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.41GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 1,197.5 ns | 38.86 ns | 58.16 ns |  1.00 |    0.07 |    3 | 0.0324 | 0.0305 |     600 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         |   751.9 ns | 43.21 ns | 64.67 ns |  0.63 |    0.06 |    2 | 0.0248 | 0.0238 |     440 B |        0.73 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   594.3 ns | 20.98 ns | 31.40 ns |  0.50 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        0.73 |
