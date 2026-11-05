```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error     | StdDev   | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|----------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 1,851.2 ns |  94.72 ns |  5.19 ns |  1.00 |    2 | 0.0744 | 0.0362 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   633.5 ns | 205.40 ns | 11.26 ns |  0.34 |    1 | 0.0391 | 0.0191 |     672 B |        0.54 |
| &#39;Policy: get by ID&#39;                            | 1,573.8 ns |  59.13 ns |  3.24 ns |  0.85 |    2 | 0.0515 | 0.0248 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,414.0 ns | 280.70 ns | 15.39 ns |  0.76 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
