```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 2.476 μs | 2.4364 μs | 0.1335 μs |  1.00 |    0.07 |    2 | 0.0534 | 0.0267 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  | 1.267 μs | 0.3613 μs | 0.0198 μs |  0.51 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        0.39 |
| &#39;Request revision&#39;   | 1.222 μs | 0.2177 μs | 0.0119 μs |  0.49 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        0.39 |
| &#39;Expire assessment&#39;  | 1.116 μs | 0.4863 μs | 0.0267 μs |  0.45 |    0.02 |    1 | 0.0210 | 0.0191 |     352 B |        0.37 |
