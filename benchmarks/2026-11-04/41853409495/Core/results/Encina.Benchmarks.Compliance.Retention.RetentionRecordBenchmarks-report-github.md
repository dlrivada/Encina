```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 2.444 μs | 0.5566 μs | 0.0305 μs |  1.00 |    0.02 |    2 | 0.0648 | 0.0305 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.503 μs | 0.4144 μs | 0.0227 μs |  0.62 |    0.01 |    1 | 0.0362 | 0.0172 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.484 μs | 0.6191 μs | 0.0339 μs |  0.61 |    0.01 |    1 | 0.0362 | 0.0172 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.464 μs | 0.5824 μs | 0.0319 μs |  0.60 |    0.01 |    1 | 0.0362 | 0.0172 |     608 B |        0.56 |
