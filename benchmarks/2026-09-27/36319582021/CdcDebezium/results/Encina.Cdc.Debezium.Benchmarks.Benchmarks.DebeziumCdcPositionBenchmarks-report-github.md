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
| CreatePosition |  4.696 ns | 0.4294 ns | 0.0235 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 30.440 ns | 5.8017 ns | 0.3180 ns |  6.48 |    0.07 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 24.489 ns | 5.5068 ns | 0.3018 ns |  5.22 |    0.06 | 0.0091 |     152 B |        6.33 |
