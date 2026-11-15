```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 3.389 μs | 0.0273 μs | 0.0409 μs |  1.00 |    0.02 |    2 | 0.0648 | 0.0305 |    1152 B |        1.00 |
| &#39;Record: mark expired&#39;               | 2.066 μs | 0.0205 μs | 0.0287 μs |  0.61 |    0.01 |    1 | 0.0343 | 0.0153 |     672 B |        0.58 |
| &#39;Record: mark deleted (terminal)&#39;    | 2.114 μs | 0.0342 μs | 0.0512 μs |  0.62 |    0.02 |    1 | 0.0343 | 0.0153 |     672 B |        0.58 |
| &#39;Record: mark anonymized (terminal)&#39; | 2.115 μs | 0.0342 μs | 0.0513 μs |  0.62 |    0.02 |    1 | 0.0343 | 0.0153 |     672 B |        0.58 |
