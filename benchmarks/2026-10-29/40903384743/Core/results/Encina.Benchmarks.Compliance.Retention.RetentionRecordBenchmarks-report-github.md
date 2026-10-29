```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 2.630 μs | 3.0434 μs | 0.1668 μs |  1.00 |    0.08 |    2 | 0.0648 | 0.0305 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.572 μs | 0.2118 μs | 0.0116 μs |  0.60 |    0.03 |    1 | 0.0362 | 0.0172 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.578 μs | 1.3624 μs | 0.0747 μs |  0.60 |    0.04 |    1 | 0.0362 | 0.0172 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.555 μs | 1.5660 μs | 0.0858 μs |  0.59 |    0.04 |    1 | 0.0362 | 0.0172 |     608 B |        0.56 |
