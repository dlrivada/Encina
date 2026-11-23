```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 2.910 μs | 0.3892 μs | 0.0213 μs |  1.00 |    3 | 0.0572 | 0.0267 |     960 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 2.343 μs | 0.1421 μs | 0.0078 μs |  0.81 |    2 | 0.0381 | 0.0191 |     680 B |        0.71 |
| &#39;Legal hold: has active holds (read-only)&#39; | 1.094 μs | 0.7928 μs | 0.0435 μs |  0.38 |    1 | 0.0343 | 0.0172 |     576 B |        0.60 |
