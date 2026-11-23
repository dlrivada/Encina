```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.713 ns |  0.6558 ns | 0.0359 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 39.043 ns |  8.8069 ns | 0.4827 ns |  5.82 |    0.07 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 31.666 ns | 15.7301 ns | 0.8622 ns |  4.72 |    0.11 | 0.0091 |     152 B |        6.33 |
