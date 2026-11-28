```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 3.297 μs | 0.4844 μs | 0.0266 μs |  1.00 |    2 | 0.0648 | 0.0305 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.844 μs | 0.3768 μs | 0.0207 μs |  0.56 |    1 | 0.0362 | 0.0172 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.891 μs | 0.5495 μs | 0.0301 μs |  0.57 |    1 | 0.0362 | 0.0172 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.809 μs | 0.3385 μs | 0.0186 μs |  0.55 |    1 | 0.0362 | 0.0172 |     608 B |        0.56 |
