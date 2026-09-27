```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.380 ns | 1.049 ns | 0.0575 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 49.035 ns | 8.660 ns | 0.4747 ns |  5.85 |    0.06 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 39.796 ns | 1.894 ns | 0.1038 ns |  4.75 |    0.03 | 0.0091 |     152 B |        6.33 |
