```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1,205.1 ns | 277.5 ns | 15.21 ns |  1.00 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,054.1 ns | 329.2 ns | 18.05 ns |  0.87 |    0.02 |    1 | 0.0229 | 0.0210 |     392 B |        0.92 |
| &#39;Get expired assessments&#39;                            | 1,009.8 ns | 427.7 ns | 23.45 ns |  0.84 |    0.02 |    1 | 0.0229 | 0.0210 |     384 B |        0.91 |
| &#39;Get all assessments&#39;                                |   938.8 ns | 210.0 ns | 11.51 ns |  0.78 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.94 |
