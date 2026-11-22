```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 2.504 μs | 0.0266 μs | 0.0390 μs |  1.00 |    0.02 |    3 | 0.0572 | 0.0267 |    1024 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 2.147 μs | 0.0291 μs | 0.0436 μs |  0.86 |    0.02 |    2 | 0.0381 | 0.0191 |     744 B |        0.73 |
| &#39;Legal hold: has active holds (read-only)&#39; | 1.127 μs | 0.0186 μs | 0.0278 μs |  0.45 |    0.01 |    1 | 0.0343 | 0.0172 |     608 B |        0.59 |
