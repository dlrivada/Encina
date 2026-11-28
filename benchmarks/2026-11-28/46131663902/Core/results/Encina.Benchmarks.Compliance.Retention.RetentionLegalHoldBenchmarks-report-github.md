```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.05GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 2.717 μs | 0.7357 μs | 0.0403 μs |  1.00 |    0.02 |    3 | 0.0572 | 0.0267 |     960 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 2.072 μs | 0.8624 μs | 0.0473 μs |  0.76 |    0.02 |    2 | 0.0381 | 0.0191 |     680 B |        0.71 |
| &#39;Legal hold: has active holds (read-only)&#39; | 1.030 μs | 0.2496 μs | 0.0137 μs |  0.38 |    0.01 |    1 | 0.0343 | 0.0172 |     576 B |        0.60 |
