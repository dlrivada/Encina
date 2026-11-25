```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 2.343 μs | 0.7604 μs | 0.0417 μs |  1.00 |    0.02 |    2 | 0.0114 | 0.0076 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.364 μs | 0.3135 μs | 0.0172 μs |  0.58 |    0.01 |    1 | 0.0057 | 0.0038 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.332 μs | 0.4463 μs | 0.0245 μs |  0.57 |    0.01 |    1 | 0.0057 | 0.0038 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.301 μs | 0.4058 μs | 0.0222 μs |  0.56 |    0.01 |    1 | 0.0057 | 0.0038 |     608 B |        0.56 |
