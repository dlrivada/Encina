```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 1,621.0 ns | 40.27 ns | 56.46 ns |  1.00 |    0.05 |    2 | 0.0324 | 0.0305 |     600 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         |   858.2 ns | 21.83 ns | 32.00 ns |  0.53 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        0.73 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   821.9 ns | 26.14 ns | 39.13 ns |  0.51 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        0.73 |
