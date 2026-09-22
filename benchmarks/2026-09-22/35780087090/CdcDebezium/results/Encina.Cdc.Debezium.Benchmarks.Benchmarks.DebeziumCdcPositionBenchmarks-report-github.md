```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.584 ns | 2.890 ns | 0.1584 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 41.193 ns | 8.737 ns | 0.4789 ns |  6.26 |    0.14 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 31.977 ns | 1.274 ns | 0.0698 ns |  4.86 |    0.10 | 0.0091 |     152 B |        6.33 |
