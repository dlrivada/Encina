```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,356.7 ns | 50.11 ns | 73.45 ns |  1.00 |    0.08 |    4 | 0.0553 | 0.0267 |     976 B |        1.00 |
| &#39;Reject assessment&#39;  |   790.0 ns | 12.68 ns | 18.19 ns |  0.58 |    0.03 |    3 | 0.0219 | 0.0210 |     384 B |        0.39 |
| &#39;Request revision&#39;   |   741.4 ns | 16.96 ns | 25.38 ns |  0.55 |    0.03 |    2 | 0.0219 | 0.0210 |     384 B |        0.39 |
| &#39;Expire assessment&#39;  |   627.9 ns | 17.33 ns | 24.85 ns |  0.46 |    0.03 |    1 | 0.0210 | 0.0200 |     368 B |        0.38 |
