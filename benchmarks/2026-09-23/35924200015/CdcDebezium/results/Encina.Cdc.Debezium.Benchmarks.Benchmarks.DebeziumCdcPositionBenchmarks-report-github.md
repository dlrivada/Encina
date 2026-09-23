```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  3.890 ns | 0.5148 ns | 0.0282 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 23.997 ns | 2.3646 ns | 0.1296 ns |  6.17 |    0.05 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 19.539 ns | 3.4583 ns | 0.1896 ns |  5.02 |    0.05 | 0.0091 |     152 B |        6.33 |
