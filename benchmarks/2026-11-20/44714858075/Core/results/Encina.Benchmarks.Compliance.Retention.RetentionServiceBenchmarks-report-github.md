```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 1,903.9 ns | 1,253.90 ns | 68.73 ns |  1.00 |    0.04 |    2 | 0.0744 | 0.0362 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   673.0 ns |    90.73 ns |  4.97 ns |  0.35 |    0.01 |    1 | 0.0391 | 0.0191 |     672 B |        0.54 |
| &#39;Policy: get by ID&#39;                            | 1,625.6 ns |   316.41 ns | 17.34 ns |  0.85 |    0.03 |    2 | 0.0515 | 0.0248 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,478.9 ns |   981.13 ns | 53.78 ns |  0.78 |    0.03 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
