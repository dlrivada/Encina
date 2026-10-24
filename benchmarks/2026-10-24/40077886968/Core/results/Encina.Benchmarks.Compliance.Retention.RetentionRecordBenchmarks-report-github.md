```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 2.532 μs | 1.1977 μs | 0.0657 μs |  1.00 |    0.03 |    2 | 0.0114 | 0.0076 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.464 μs | 0.2996 μs | 0.0164 μs |  0.58 |    0.01 |    1 | 0.0057 | 0.0038 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.479 μs | 0.7572 μs | 0.0415 μs |  0.58 |    0.02 |    1 | 0.0057 | 0.0038 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.439 μs | 0.3356 μs | 0.0184 μs |  0.57 |    0.01 |    1 | 0.0057 | 0.0038 |     608 B |        0.56 |
