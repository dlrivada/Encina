```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 1,900.7 ns | 488.0 ns | 26.75 ns |  1.00 |    0.02 |    2 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   627.7 ns | 262.8 ns | 14.41 ns |  0.33 |    0.01 |    1 | 0.0391 | 0.0191 |     672 B |        0.54 |
| &#39;Policy: get by ID&#39;                            | 1,591.2 ns | 585.3 ns | 32.08 ns |  0.84 |    0.02 |    2 | 0.0515 | 0.0248 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,354.0 ns | 472.3 ns | 25.89 ns |  0.71 |    0.01 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
