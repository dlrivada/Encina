```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.40GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 2.424 μs | 1.3249 μs | 0.0726 μs |  1.00 |    0.04 |    2 | 0.0114 | 0.0076 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.414 μs | 0.3783 μs | 0.0207 μs |  0.58 |    0.02 |    1 | 0.0057 | 0.0038 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.382 μs | 0.4047 μs | 0.0222 μs |  0.57 |    0.02 |    1 | 0.0057 | 0.0038 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.378 μs | 0.3779 μs | 0.0207 μs |  0.57 |    0.02 |    1 | 0.0057 | 0.0038 |     608 B |        0.56 |
