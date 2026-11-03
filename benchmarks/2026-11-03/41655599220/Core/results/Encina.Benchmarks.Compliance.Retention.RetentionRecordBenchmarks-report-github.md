```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.81GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 4.186 μs | 2.2261 μs | 0.1220 μs |  1.00 |    0.04 |    2 | 0.0610 | 0.0305 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 2.386 μs | 0.2322 μs | 0.0127 μs |  0.57 |    0.01 |    1 | 0.0343 | 0.0153 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 2.354 μs | 0.6479 μs | 0.0355 μs |  0.56 |    0.02 |    1 | 0.0343 | 0.0153 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 2.349 μs | 0.9797 μs | 0.0537 μs |  0.56 |    0.02 |    1 | 0.0343 | 0.0153 |     608 B |        0.56 |
