```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 3.553 μs | 0.0393 μs | 0.0576 μs |  1.00 |    0.02 |    3 | 0.0648 | 0.0305 |    1152 B |        1.00 |
| &#39;Record: mark expired&#39;               | 2.213 μs | 0.0407 μs | 0.0609 μs |  0.62 |    0.02 |    2 | 0.0343 | 0.0153 |     672 B |        0.58 |
| &#39;Record: mark deleted (terminal)&#39;    | 2.077 μs | 0.0293 μs | 0.0438 μs |  0.58 |    0.02 |    1 | 0.0343 | 0.0153 |     672 B |        0.58 |
| &#39;Record: mark anonymized (terminal)&#39; | 2.068 μs | 0.0232 μs | 0.0340 μs |  0.58 |    0.01 |    1 | 0.0343 | 0.0153 |     672 B |        0.58 |
