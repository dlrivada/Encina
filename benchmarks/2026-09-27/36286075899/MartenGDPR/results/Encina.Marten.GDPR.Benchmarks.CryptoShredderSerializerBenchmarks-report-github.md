```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                    | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   580.3 ns | 15.85 ns | 21.70 ns |   599.0 ns |  1.00 |    0.05 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   598.6 ns |  2.51 ns |  3.60 ns |   597.9 ns |  1.03 |    0.04 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 7,980.4 ns | 17.35 ns | 24.88 ns | 7,977.3 ns | 13.77 |    0.51 |    2 | 0.1678 |    2869 B |       10.55 |
