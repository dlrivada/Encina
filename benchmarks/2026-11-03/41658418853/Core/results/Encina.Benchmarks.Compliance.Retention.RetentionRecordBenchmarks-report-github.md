```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 3.100 μs | 0.4536 μs | 0.0249 μs |  1.00 |    2 | 0.0420 | 0.0191 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.909 μs | 0.6595 μs | 0.0361 μs |  0.62 |    1 | 0.0229 | 0.0191 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.819 μs | 0.0544 μs | 0.0030 μs |  0.59 |    1 | 0.0229 | 0.0210 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.797 μs | 0.2281 μs | 0.0125 μs |  0.58 |    1 | 0.0229 | 0.0210 |     608 B |        0.56 |
