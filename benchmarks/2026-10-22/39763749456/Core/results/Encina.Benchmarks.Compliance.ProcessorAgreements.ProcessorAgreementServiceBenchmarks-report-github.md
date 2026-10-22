```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         |   989.3 ns |   651.9 ns | 35.73 ns |  1.00 |    0.04 |    2 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,017.7 ns |   737.0 ns | 40.40 ns |  1.03 |    0.05 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              |   917.7 ns | 1,537.0 ns | 84.25 ns |  0.93 |    0.08 |    2 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;            |   873.9 ns |   158.4 ns |  8.68 ns |  0.88 |    0.03 |    2 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   688.7 ns |   301.9 ns | 16.55 ns |  0.70 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
