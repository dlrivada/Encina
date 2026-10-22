```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 2.605 μs | 0.0357 μs | 0.0534 μs |  1.00 |    0.03 |    3 | 0.0572 | 0.0267 |    1024 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 2.231 μs | 0.0223 μs | 0.0333 μs |  0.86 |    0.02 |    2 | 0.0381 | 0.0191 |     744 B |        0.73 |
| &#39;Legal hold: has active holds (read-only)&#39; | 1.177 μs | 0.0328 μs | 0.0491 μs |  0.45 |    0.02 |    1 | 0.0343 | 0.0172 |     608 B |        0.59 |
