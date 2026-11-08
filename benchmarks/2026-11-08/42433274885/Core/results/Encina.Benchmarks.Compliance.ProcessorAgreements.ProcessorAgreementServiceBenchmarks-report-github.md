```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 1,086.6 ns | 18.47 ns | 27.07 ns |  1.00 |    0.03 |    3 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,181.7 ns | 20.60 ns | 30.83 ns |  1.09 |    0.04 |    4 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetDPA by ID (cached read)&#39;              | 1,033.2 ns | 14.05 ns | 21.03 ns |  0.95 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;            |   929.3 ns | 60.88 ns | 91.12 ns |  0.86 |    0.09 |    2 | 0.0248 | 0.0238 |     440 B |        1.15 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   704.9 ns | 29.44 ns | 44.06 ns |  0.65 |    0.04 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
