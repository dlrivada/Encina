```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 2.534 μs | 0.0547 μs | 0.0802 μs |  1.00 |    0.04 |    3 | 0.0534 | 0.0267 |    1008 B |        1.00 |
| &#39;Reject assessment&#39;  | 1.344 μs | 0.0140 μs | 0.0196 μs |  0.53 |    0.02 |    2 | 0.0210 | 0.0191 |     400 B |        0.40 |
| &#39;Request revision&#39;   | 1.363 μs | 0.0219 μs | 0.0314 μs |  0.54 |    0.02 |    2 | 0.0210 | 0.0191 |     400 B |        0.40 |
| &#39;Expire assessment&#39;  | 1.130 μs | 0.0149 μs | 0.0223 μs |  0.45 |    0.02 |    1 | 0.0210 | 0.0191 |     384 B |        0.38 |
