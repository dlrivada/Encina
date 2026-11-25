```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1,013.1 ns | 825.3 ns | 45.24 ns |  1.00 |    0.06 |    1 | 0.0048 | 0.0038 |     440 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   853.0 ns | 119.1 ns |  6.53 ns |  0.84 |    0.03 |    1 | 0.0038 | 0.0029 |     408 B |        0.93 |
| &#39;Get expired assessments&#39;                            |   814.2 ns | 557.7 ns | 30.57 ns |  0.80 |    0.04 |    1 | 0.0038 | 0.0029 |     400 B |        0.91 |
| &#39;Get all assessments&#39;                                |   755.5 ns | 530.7 ns | 29.09 ns |  0.75 |    0.04 |    1 | 0.0038 | 0.0029 |     400 B |        0.91 |
