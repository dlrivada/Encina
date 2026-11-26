```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 2.552 μs | 1.4193 μs | 0.0778 μs |  1.00 |    0.04 |    2 | 0.0114 | 0.0076 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.532 μs | 0.5108 μs | 0.0280 μs |  0.60 |    0.02 |    1 | 0.0057 | 0.0038 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.529 μs | 0.2083 μs | 0.0114 μs |  0.60 |    0.02 |    1 | 0.0057 | 0.0038 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.529 μs | 0.1042 μs | 0.0057 μs |  0.60 |    0.02 |    1 | 0.0057 | 0.0038 |     608 B |        0.56 |
