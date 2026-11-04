```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1,041.6 ns | 269.1 ns | 14.75 ns |  1.00 |    0.02 |    2 | 0.0048 | 0.0038 |     440 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   861.3 ns | 331.7 ns | 18.18 ns |  0.83 |    0.02 |    1 | 0.0038 | 0.0029 |     408 B |        0.93 |
| &#39;Get expired assessments&#39;                            |   821.6 ns | 295.9 ns | 16.22 ns |  0.79 |    0.02 |    1 | 0.0038 | 0.0029 |     400 B |        0.91 |
| &#39;Get all assessments&#39;                                |   758.3 ns | 444.8 ns | 24.38 ns |  0.73 |    0.02 |    1 | 0.0038 | 0.0029 |     400 B |        0.91 |
