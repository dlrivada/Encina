```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.93GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1,074.2 ns | 22.65 ns | 33.90 ns |  1.00 |    0.04 |    3 | 0.0248 | 0.0229 |     456 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   996.8 ns | 24.95 ns | 37.35 ns |  0.93 |    0.04 |    2 | 0.0229 | 0.0210 |     424 B |        0.93 |
| &#39;Get expired assessments&#39;                            |   932.5 ns | 20.73 ns | 31.03 ns |  0.87 |    0.04 |    1 | 0.0229 | 0.0219 |     400 B |        0.88 |
| &#39;Get all assessments&#39;                                |   892.5 ns | 24.09 ns | 36.06 ns |  0.83 |    0.04 |    1 | 0.0229 | 0.0219 |     400 B |        0.88 |
