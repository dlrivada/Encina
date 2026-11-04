```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.40GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 1,089.9 ns |   666.0 ns | 36.51 ns |  1.00 |    0.04 |    1 | 0.0038 | 0.0029 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; |   900.5 ns | 1,037.6 ns | 56.88 ns |  0.83 |    0.05 |    1 | 0.0038 | 0.0019 |     424 B |        1.15 |
| &#39;GetDPA by ID (cached read)&#39;              | 1,048.2 ns |   928.1 ns | 50.87 ns |  0.96 |    0.05 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            | 1,013.2 ns |   735.9 ns | 40.34 ns |  0.93 |    0.04 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   837.7 ns |   571.6 ns | 31.33 ns |  0.77 |    0.03 |    1 | 0.0038 | 0.0029 |     400 B |        1.09 |
