```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1.230 μs | 0.1207 μs | 0.0066 μs |  1.00 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1.169 μs | 0.2791 μs | 0.0153 μs |  0.95 |    0.01 |    1 | 0.0153 | 0.0134 |     392 B |        0.92 |
| &#39;Get expired assessments&#39;                            | 1.028 μs | 0.7597 μs | 0.0416 μs |  0.84 |    0.03 |    1 | 0.0153 | 0.0134 |     384 B |        0.91 |
| &#39;Get all assessments&#39;                                | 1.031 μs | 0.5773 μs | 0.0316 μs |  0.84 |    0.02 |    1 | 0.0153 | 0.0134 |     384 B |        0.91 |
