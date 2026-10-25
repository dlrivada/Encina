```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 3.059 μs | 0.9505 μs | 0.0521 μs |  1.00 |    0.02 |    2 | 0.0648 | 0.0305 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.737 μs | 0.5864 μs | 0.0321 μs |  0.57 |    0.01 |    1 | 0.0362 | 0.0172 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.707 μs | 0.2519 μs | 0.0138 μs |  0.56 |    0.01 |    1 | 0.0362 | 0.0172 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.678 μs | 0.3038 μs | 0.0167 μs |  0.55 |    0.01 |    1 | 0.0362 | 0.0172 |     608 B |        0.56 |
