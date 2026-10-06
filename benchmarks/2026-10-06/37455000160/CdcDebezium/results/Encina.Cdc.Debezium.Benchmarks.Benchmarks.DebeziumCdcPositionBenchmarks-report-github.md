```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.998 ns |  2.308 ns | 0.1265 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 54.293 ns | 21.190 ns | 1.1615 ns |  6.03 |    0.13 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 47.428 ns | 15.874 ns | 0.8701 ns |  5.27 |    0.11 | 0.0091 |     152 B |        6.33 |
