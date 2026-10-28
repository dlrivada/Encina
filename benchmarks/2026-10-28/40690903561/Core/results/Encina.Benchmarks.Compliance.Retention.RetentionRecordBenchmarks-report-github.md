```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 3.374 μs | 0.3949 μs | 0.0216 μs |  1.00 |    2 | 0.0648 | 0.0305 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 2.045 μs | 0.3161 μs | 0.0173 μs |  0.61 |    1 | 0.0343 | 0.0153 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 2.020 μs | 0.3329 μs | 0.0182 μs |  0.60 |    1 | 0.0343 | 0.0153 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.963 μs | 0.7231 μs | 0.0396 μs |  0.58 |    1 | 0.0343 | 0.0153 |     608 B |        0.56 |
