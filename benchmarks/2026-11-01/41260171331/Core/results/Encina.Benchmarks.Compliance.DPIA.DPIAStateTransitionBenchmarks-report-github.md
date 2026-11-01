```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 2.218 μs | 0.0177 μs | 0.0259 μs |  1.00 |    0.02 |    3 | 0.0534 | 0.0267 |    1008 B |        1.00 |
| &#39;Reject assessment&#39;  | 1.272 μs | 0.0166 μs | 0.0248 μs |  0.57 |    0.01 |    2 | 0.0210 | 0.0191 |     400 B |        0.40 |
| &#39;Request revision&#39;   | 1.245 μs | 0.0117 μs | 0.0175 μs |  0.56 |    0.01 |    2 | 0.0210 | 0.0191 |     400 B |        0.40 |
| &#39;Expire assessment&#39;  | 1.097 μs | 0.0149 μs | 0.0222 μs |  0.49 |    0.01 |    1 | 0.0210 | 0.0191 |     384 B |        0.38 |
