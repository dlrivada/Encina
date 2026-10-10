```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.078 ns |  1.209 ns | 0.0663 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 56.644 ns | 19.090 ns | 1.0464 ns |  7.01 |    0.12 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 48.983 ns |  5.345 ns | 0.2930 ns |  6.06 |    0.05 | 0.0091 |     152 B |        6.33 |
