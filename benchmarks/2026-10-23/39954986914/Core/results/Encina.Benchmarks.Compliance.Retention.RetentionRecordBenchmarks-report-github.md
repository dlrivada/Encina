```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 3.260 μs | 0.1838 μs | 0.0101 μs |  1.00 |    2 | 0.0648 | 0.0305 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 2.013 μs | 0.4912 μs | 0.0269 μs |  0.62 |    1 | 0.0343 | 0.0153 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.948 μs | 0.1358 μs | 0.0074 μs |  0.60 |    1 | 0.0343 | 0.0153 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.950 μs | 0.1901 μs | 0.0104 μs |  0.60 |    1 | 0.0343 | 0.0153 |     608 B |        0.56 |
