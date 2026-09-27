```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   630.2 ns |    17.14 ns |   0.94 ns |  1.00 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   654.5 ns |     5.22 ns |   0.29 ns |  1.04 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 8,661.0 ns | 2,443.72 ns | 133.95 ns | 13.74 |    0.18 |    2 | 0.1678 |    2869 B |       10.55 |
