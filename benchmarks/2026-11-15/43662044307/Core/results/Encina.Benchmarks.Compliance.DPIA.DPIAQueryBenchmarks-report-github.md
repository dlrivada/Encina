```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1,166.0 ns | 24.44 ns | 36.58 ns |  1.00 |    0.04 |    2 | 0.0248 | 0.0229 |     456 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,017.0 ns | 28.31 ns | 41.49 ns |  0.87 |    0.04 |    1 | 0.0229 | 0.0210 |     424 B |        0.93 |
| &#39;Get expired assessments&#39;                            |   991.3 ns | 23.30 ns | 34.88 ns |  0.85 |    0.04 |    1 | 0.0229 | 0.0219 |     400 B |        0.88 |
| &#39;Get all assessments&#39;                                |   961.8 ns | 12.84 ns | 18.81 ns |  0.83 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        0.88 |
