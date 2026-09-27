```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error    | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|---------:|--------:|------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   440.5 ns |  8.65 ns | 0.47 ns |  1.00 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   517.1 ns | 27.40 ns | 1.50 ns |  1.17 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 6,497.7 ns | 41.17 ns | 2.26 ns | 14.75 |    2 | 0.1678 |    2869 B |       10.55 |
