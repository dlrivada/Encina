```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 2.837 μs | 0.1617 μs | 0.0089 μs |  1.00 |    2 | 0.0420 | 0.0191 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.790 μs | 0.5174 μs | 0.0284 μs |  0.63 |    1 | 0.0229 | 0.0210 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.729 μs | 0.4924 μs | 0.0270 μs |  0.61 |    1 | 0.0229 | 0.0210 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.720 μs | 0.2137 μs | 0.0117 μs |  0.61 |    1 | 0.0229 | 0.0210 |     608 B |        0.56 |
