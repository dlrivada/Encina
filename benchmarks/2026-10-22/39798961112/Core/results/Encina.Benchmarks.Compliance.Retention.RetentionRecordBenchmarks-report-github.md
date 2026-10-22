```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 2.651 μs | 0.0482 μs | 0.0691 μs |  1.00 |    0.04 |    2 | 0.0648 | 0.0305 |    1152 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.669 μs | 0.0239 μs | 0.0358 μs |  0.63 |    0.02 |    1 | 0.0362 | 0.0172 |     640 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.663 μs | 0.0203 μs | 0.0297 μs |  0.63 |    0.02 |    1 | 0.0362 | 0.0172 |     640 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.628 μs | 0.0227 μs | 0.0319 μs |  0.61 |    0.02 |    1 | 0.0362 | 0.0172 |     640 B |        0.56 |
