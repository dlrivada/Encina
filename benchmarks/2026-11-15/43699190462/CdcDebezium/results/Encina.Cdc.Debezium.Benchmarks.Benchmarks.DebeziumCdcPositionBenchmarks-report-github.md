```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.165 ns |  0.1583 ns | 0.0087 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 39.445 ns | 14.5476 ns | 0.7974 ns |  5.51 |    0.10 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 31.070 ns |  0.8877 ns | 0.0487 ns |  4.34 |    0.01 | 0.0091 |     152 B |        6.33 |
