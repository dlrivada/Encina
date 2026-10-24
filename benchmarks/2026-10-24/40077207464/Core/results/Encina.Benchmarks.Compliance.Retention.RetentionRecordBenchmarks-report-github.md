```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 3.347 μs | 0.8471 μs | 0.0464 μs |  1.00 |    0.02 |    2 | 0.0648 | 0.0305 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 2.066 μs | 0.6715 μs | 0.0368 μs |  0.62 |    0.01 |    1 | 0.0343 | 0.0153 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 2.035 μs | 0.2032 μs | 0.0111 μs |  0.61 |    0.01 |    1 | 0.0343 | 0.0153 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 2.044 μs | 0.3556 μs | 0.0195 μs |  0.61 |    0.01 |    1 | 0.0343 | 0.0153 |     608 B |        0.56 |
