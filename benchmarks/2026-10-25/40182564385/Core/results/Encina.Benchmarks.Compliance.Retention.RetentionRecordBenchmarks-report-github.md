```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 3.013 μs | 0.4232 μs | 0.0232 μs |  1.00 |    2 | 0.0420 | 0.0191 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.840 μs | 0.1268 μs | 0.0069 μs |  0.61 |    1 | 0.0229 | 0.0210 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.800 μs | 0.1325 μs | 0.0073 μs |  0.60 |    1 | 0.0229 | 0.0210 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.789 μs | 0.0557 μs | 0.0031 μs |  0.59 |    1 | 0.0229 | 0.0210 |     608 B |        0.56 |
