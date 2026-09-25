```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   559.9 ns |  53.44 ns | 2.93 ns |  1.00 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   604.3 ns |  17.83 ns | 0.98 ns |  1.08 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 8,581.7 ns | 115.26 ns | 6.32 ns | 15.33 |    0.07 |    2 | 0.1678 |    2869 B |       10.55 |
